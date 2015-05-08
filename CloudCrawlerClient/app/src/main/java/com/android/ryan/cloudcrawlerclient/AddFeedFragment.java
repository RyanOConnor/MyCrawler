package com.android.ryan.cloudcrawlerclient;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import android.support.v4.app.Fragment;
import android.text.InputType;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.EditText;
/**
 * Created by Ryan on 4/15/2015.
 */
public class AddFeedFragment extends Fragment {

    private View addFeedView;
    private EditText urlBar;
    private Button loadPage;
    private static final String ARG_SECTION_NUMBER = "section_number";

    public static AddFeedFragment newInstance(int sectionNumber) {
        AddFeedFragment fragment = new AddFeedFragment();
        Bundle args = new Bundle();
        args.putInt(ARG_SECTION_NUMBER, sectionNumber);
        fragment.setArguments(args);
        return fragment;
    }

    public AddFeedFragment() {
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container,
                             Bundle savedInstanceState) {
        addFeedView = inflater.inflate(R.layout.fragment_add_feed, container, false);

        urlBar = (EditText)addFeedView.findViewById(R.id.urlBar);
        urlBar.setInputType(InputType.TYPE_TEXT_FLAG_NO_SUGGESTIONS);
        loadPage = (Button)addFeedView.findViewById(R.id.goButton);

        loadPage.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                Intent intent = new Intent(getActivity(), TargetContentActivity.class);
                intent.putExtra("url", urlBar.getText().toString());
                startActivity(intent);
            }
        });

        return addFeedView;
    }

    @Override
    public void onAttach(Activity activity) {
        super.onAttach(activity);
    }

    @Override
    public void onDetach() {
        super.onDetach();
    }
}
